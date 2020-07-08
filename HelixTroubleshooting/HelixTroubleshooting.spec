# -*- mode: python -*-

block_cipher = None


a = Analysis(['HelixTroubleshooting.py'],
             pathex=['C:\\Users\\grobinson\\source\\repos\\EvoTroubleTools\\EvoTroubleTools\\'],
             binaries=[],
             datas=[
			 ('C:\\Users\\grobinson\\source\\repos\\EvoTroubleTools\\EvoTroubleTools\\Images\\bar.png','images'),
			 ('C:\\Users\\grobinson\\source\\repos\\EvoTroubleTools\\EvoTroubleTools\\Images\\bar.ico','images')
			 ],
             hiddenimports=[],
             hookspath=[],
             runtime_hooks=[],
             excludes=[],
             win_no_prefer_redirects=False,
             win_private_assemblies=False,
             cipher=block_cipher)
pyz = PYZ(a.pure, a.zipped_data,
             cipher=block_cipher)
exe = EXE(pyz,
          a.scripts,
          a.binaries,
          a.zipfiles,
          a.datas,
          name='Helix Troubleshooting',
          debug=False,
          strip=False,
          upx=True,
          runtime_tmpdir=None,
          console=False , icon='bar.ico')
